# Blend Umbraco AltTextAI
-----

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

This is a simple package that integrates the [AltText.AI](https://alttext.ai) text generation 
service with the Umbraco media library.

The package will send all image media objects that have an empty alt text field to the 
alttext.ai service and add the AI-generated alt text to the media object. The generation 
happens as a background task so that the performance of editor's workflow isn't affected.

## Installation

You can add the package to your Umbraco instance using either the command line or the Nuget package manager.

---
Command Line
```
dotnet add package Our.Umbraco.Blend.AltTextAi
```

Or Nuget
```
Install-Package Our.Umbraco.Blend.AltTextAi
```

## Configuration

First, in the Umbraco Backoffice, go to the setup tab and add a field to your image objects to 
hold your alt text. Take note of the property alias (we recommend calling the field "Alt Text", 
with an alias of "altText")

Next, you'll need to add a section to your appsettings.json file to configure the package.

```json
  "AltTextAi": {
    "ImageAltTextProperty": "altText",
    "AltTextAiApiKey": "<YOUR API KEY GOES HERE>"
  }
```

In the "ImageAltTextproperty" setting, add the alias for your alt text field you've just added. 
This is where the package will look for and write generated alt text.

In the "AltTextAiApiKey" setting, add an API Key from your AltTextAI account. You'll need 
an alttext.ai account for this. A free trial is available. 

Once you have an account, the [AltText.AI documentation explains how to create an API Key](https://alttext.ai/docs/webui/account/#api-keys).




