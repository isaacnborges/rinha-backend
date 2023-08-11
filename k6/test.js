import http from 'k6/http';
import { sleep, check } from 'k6';
import { randomString } from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';

export let options = {
    stages: [
        { duration: '30s', target: 10 },  // 10 usuários por 30 segundos
        { duration: '1m', target: 50 },   // 50 usuários por 1 minuto
        { duration: '2m', target: 100 },  // 100 usuários por 2 minutos
        { duration: '1m', target: 50 },   // 50 usuários por 1 minuto
        { duration: '30s', target: 10 },  // Reduz para 10 usuários por 30 segundos
    ],
};

let createdPersonId;

export default function () {
    createPessoa();
    searchPessoa();
    getPessoaById();
    getContagemPessoas();
}

function createPessoa() {
    let payload = {
        apelido: randomString(8),
        nome: randomString(10, `aeioubcdfghijpqrstuv`),
        nascimento: '1990-01-01',
        stack: ['K6', 'Test']
    };

    let headers = { 'Content-Type': 'application/json' };

    let res = http.post('http://localhost:9999/pessoas', JSON.stringify(payload), { headers: headers });

    createdPersonId = JSON.parse(res.body).id;

    check(res, {
        'Criação de pessoas - Status 201': (r) => r.status === 201,
    });

    sleep(1);
}

function searchPessoa() {
    let searchTerm = 'Teste';
    let res = http.get(`http://localhost:9999/pessoas?t=${searchTerm}`);

    check(res, {
        'Busca de pessoas - Status 200': (r) => r.status === 200,
    });

    sleep(1);
}

function getPessoaById() {
    let res = http.get(`http://localhost:9999/pessoas/${createdPersonId}`);

    check(res, {
        'Pessoa by Id Status 200': (r) => r.status === 200,
    });

    sleep(1);
}

function getContagemPessoas() {
    let res = http.get('http://localhost:9999/contagem-pessoas');

    check(res, {
        'Contagem pessoa - status 200': (r) => r.status === 200,
    });

    sleep(1);
}