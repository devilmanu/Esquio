import tl = require('./node_modules/azure-pipelines-task-lib/task');
import url = require('url');
const https = require('https');

async function run() {
    try {
        const esquioConnection = tl.getInput('EsquioService', true);
        const featureName: string = tl.getInput('featureName', true);
        const toggleType: string = tl.getInput('toggleType', true);
        const productName: string = tl.getInput('productName', true);
        const parameterName: string = tl.getInput('parameterName', true);
        const parameterValue: string = tl.getInput('parameterValue', true);

        const esquioUrl = url.parse(tl.getEndpointUrl(esquioConnection, false));
        const serverEndpointAuth: tl.EndpointAuthorization = tl.getEndpointAuthorization(esquioConnection, false);
        const esquioApiKey = serverEndpointAuth["parameters"]["apitoken"];

        await setToggleParameter(esquioUrl, esquioApiKey, productName, featureName, toggleType, parameterName, parameterValue);
    }
    catch (err) {
        tl.setResult(tl.TaskResult.Failed, err.message);
    }
}

async function setToggleParameter(esquioUrl: url.UrlWithStringQuery, esquioApiKey: string, productName: string, featureName: string, toggleType: string, parameterName: string, parameterValue: string) {
    const options = {
        hostname: esquioUrl.host,
        path: `/api/toggles/parameters`,
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'x-api-key': esquioApiKey
        }
    }

    var postData = JSON.stringify({
        "ProductName": productName,
        "FeatureName": featureName,
        "ToggleType": toggleType,
        "Name": parameterName,
        "Value": parameterValue
    });

    const req = https.request(options, (res: any) => {
        if (res.statusCode === 200) {
            console.log('Set toggle parameter succesful');
        }

        res.on('data', (data: any) => {
            if (res.statusCode != 200) {
                const responseData = JSON.parse(data);
                tl.setResult(tl.TaskResult.Failed, `Error set toggle parameter ${responseData.detail} HttpCode: ${res.statusCode}`);
            }
        });
    });
    req.on('error', (error: any) => {
        tl.setResult(tl.TaskResult.Failed, error);
    });

    req.write(postData);
    req.end();
}

run();