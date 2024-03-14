import os
import openai
import dotenv
import requests

dotenv.load_dotenv()

openai.api_base = os.environ.get("AZURE_OPENAI_ENDPOINT")

# Azure OpenAI on your own data is only supported by the 2023-08-01-preview API version
openai.api_version = "2023-08-01-preview"
openai.api_type = 'azure'
openai.api_key = os.environ.get("AZURE_OPENAI_API_KEY")

def setup_byod(deployment_id: str) -> None:
    """Sets up the OpenAI Python SDK to use your own data for the chat endpoint.
 
    :param deployment_id: The deployment ID for the model to use with your own data.

    To remove this configuration, simply set openai.requestssession to None.
    """

    class BringYourOwnDataAdapter(requests.adapters.HTTPAdapter):

     def send(self, request, **kwargs):
         request.url = f"{openai.api_base}/openai/deployments/{deployment_id}/extensions/chat/completions?api-version={openai.api_version}"
         return super().send(request, **kwargs)

    session = requests.Session()

    # Mount a custom adapter which will use the extensions endpoint for any call using the given `deployment_id`
    session.mount(
        prefix=f"{openai.api_base}/openai/deployments/{deployment_id}",
        adapter=BringYourOwnDataAdapter()
    )

    openai.requestssession = session

aoai_deployment_id = os.environ.get("AZURE_OPENAI_DEPLOYMENT_NAME")
setup_byod(aoai_deployment_id)

completion = openai.ChatCompletion.create(
    messages=[{"role": "user", "content": "What are my available health plans?"}],
    deployment_id=os.environ.get("AZURE_OPENAI_DEPLOYMENT_NAME"),
    dataSources=[  # camelCase is intentional, as this is the format the API expects
        {
            "type": "AzureCognitiveSearch",
            "parameters": {
                "endpoint": os.environ.get("AZURE_AI_SEARCH_ENDPOINT"),
                "key": os.environ.get("AZURE_AI_SEARCH_API_KEY"),
                "indexName": os.environ.get("AZURE_AI_SEARCH_INDEX"),
            }
        }
    ]
)
print(completion)