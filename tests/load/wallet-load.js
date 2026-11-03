import http from 'k6/http';
import exec from 'k6/execution';
import { sleep } from 'k6';
import { Counter } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://priceforge:8080';
const PREPARE = __ENV.PREPARE === '1';
const USER_COUNT = 151;
const SHARED_USER = 0;
const PASSWORD = 'LoadTest-12345!';
const SPEED_BOOST_ID = '0b9d3c1e-6a51-4f7e-9c2a-3e5f1a7b8c02';
const CLICKS_PER_SECOND = 7;
const BATCH_SECONDS = 5;
const SCENARIO_SECONDS = 30;
const GAP_SECONDS = 10;
const SETUP_BATCH = 10;
const LOGIN_INTERVAL_SECONDS = 1.05;
const STATUSES = ['0', '200', '201', '400', '409', '422', '429', '500', '503'];

const SCENARIOS = {
    buy_many_wallets: { vus: 150, exec: 'buyOwnWallet' },
    buy_one_wallet: { vus: 50, exec: 'buySharedWallet' },
    earn_one_wallet: { vus: 50, exec: 'earnSharedWallet' },
    earn_clicks: { vus: 100, exec: 'earnClicks' },
    earn_batches: { vus: 100, exec: 'earnBatches' },
};

const responses = new Counter('responses');

export const options = {
    setupTimeout: '10m',
    summaryTrendStats: ['med', 'p(95)', 'p(99)', 'max'],
    scenarios: PREPARE ? { noop: { executor: 'shared-iterations', vus: 1, iterations: 1, exec: 'noop' } } : buildScenarios(),
    thresholds: PREPARE ? {} : buildThresholds(),
};

function buildScenarios() {
    const scenarios = {};
    Object.entries(SCENARIOS).forEach(([name, scenario], index) => {
        scenarios[name] = {
            executor: 'constant-vus',
            vus: scenario.vus,
            duration: `${SCENARIO_SECONDS}s`,
            gracefulStop: '5s',
            startTime: `${index * (SCENARIO_SECONDS + GAP_SECONDS)}s`,
            exec: scenario.exec,
        };
    });
    return scenarios;
}

function buildThresholds() {
    const thresholds = {};
    Object.keys(SCENARIOS).forEach((name) => {
        thresholds[`http_req_duration{scenario:${name}}`] = ['max>=0'];
        thresholds[`http_reqs{scenario:${name}}`] = ['count>=0'];
        STATUSES.forEach((status) => {
            thresholds[`responses{scenario:${name},status:${status}}`] = ['count>=0'];
        });
    });
    return thresholds;
}

function emailOf(index) {
    return `load-${index}@priceforge.test`;
}

function jsonParams(extraHeaders = {}) {
    return { headers: { 'Content-Type': 'application/json', ...extraHeaders } };
}

function authParams(token, key) {
    return jsonParams({ Authorization: `Bearer ${token}`, 'Idempotency-Key': key });
}

function userRange() {
    return Array.from({ length: USER_COUNT }, (_, index) => index);
}

function inBatches(items, send) {
    const results = [];
    for (let start = 0; start < items.length; start += SETUP_BATCH) {
        results.push(...send(items.slice(start, start + SETUP_BATCH)));
    }
    return results;
}

function registerAll() {
    inBatches(userRange(), (batch) => http.batch(batch.map((index) => [
        'POST',
        `${BASE_URL}/api/v1/auth/register`,
        JSON.stringify({ email: emailOf(index), password: PASSWORD }),
        jsonParams(),
    ])));
}

function login(index) {
    const body = JSON.stringify({ email: emailOf(index), password: PASSWORD });
    const response = http.post(`${BASE_URL}/api/v1/auth/login`, body, jsonParams());
    if (response.status !== 200) {
        exec.test.abort(`login failed: ${response.status} ${response.body}`);
    }
    sleep(LOGIN_INTERVAL_SECONDS);
    return response.json('accessToken');
}

function loginAll() {
    return userRange().map(login);
}

function createWallets(tokens) {
    inBatches(userRange(), (batch) => http.batch(batch.map((index) => [
        'POST',
        `${BASE_URL}/api/v1/wallet/earn`,
        JSON.stringify({ coins: 1 }),
        authParams(tokens[index], 'prepare'),
    ])));
}

export function setup() {
    if (!PREPARE) {
        return { tokens: loginAll() };
    }
    registerAll();
    createWallets(loginAll());
    return { tokens: [] };
}

function uniqueKey() {
    return `${exec.vu.idInTest}-${exec.vu.iterationInScenario}-${Date.now()}`;
}

function ownToken(data) {
    return data.tokens[exec.vu.idInTest];
}

function record(response) {
    responses.add(1, { status: String(response.status) });
}

function buy(token) {
    const body = JSON.stringify({ productId: SPEED_BOOST_ID });
    record(http.post(`${BASE_URL}/api/v1/game/purchases`, body, authParams(token, uniqueKey())));
}

function earn(token, coins) {
    const body = JSON.stringify({ coins });
    record(http.post(`${BASE_URL}/api/v1/wallet/earn`, body, authParams(token, uniqueKey())));
}

export function noop() {
}

export function buyOwnWallet(data) {
    buy(ownToken(data));
}

export function buySharedWallet(data) {
    buy(data.tokens[SHARED_USER]);
}

export function earnSharedWallet(data) {
    earn(data.tokens[SHARED_USER], 1);
}

export function earnClicks(data) {
    earn(ownToken(data), 1);
    sleep(1 / CLICKS_PER_SECOND);
}

export function earnBatches(data) {
    if (exec.vu.iterationInScenario === 0) {
        sleep(Math.random() * BATCH_SECONDS);
    }
    earn(ownToken(data), CLICKS_PER_SECOND * BATCH_SECONDS);
    sleep(BATCH_SECONDS);
}

function scenarioRow(metrics, name) {
    const duration = metrics[`http_req_duration{scenario:${name}}`].values;
    const count = metrics[`http_reqs{scenario:${name}}`].values.count;
    const statuses = STATUSES
        .map((status) => [status, metrics[`responses{scenario:${name},status:${status}}`].values.count])
        .filter(([, statusCount]) => statusCount > 0)
        .map(([status, statusCount]) => `${status}:${statusCount}`)
        .join(' ');
    const rps = (count / SCENARIO_SECONDS).toFixed(0);
    const timings = [duration.med, duration['p(95)'], duration['p(99)'], duration.max].map((ms) => ms.toFixed(0)).join(' / ');
    return `${name.padEnd(18)} ${rps.padStart(5)} rps   ${timings.padEnd(26)} ${statuses}`;
}

export function handleSummary(data) {
    if (PREPARE) {
        return { stdout: 'prepared\n' };
    }
    const header = `${'scenario'.padEnd(18)} ${'rate'.padStart(9)}   ${'med / p95 / p99 / max ms'.padEnd(26)} statuses`;
    const rows = Object.keys(SCENARIOS).map((name) => scenarioRow(data.metrics, name));
    return { stdout: `${[header, ...rows].join('\n')}\n` };
}
