# Blend Umbraco AltTextAI
-----

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![NuGet version (BlendInteractive.Umbraco.AltTextAi)](https://img.shields.io/nuget/v/Our.Umbraco.Blend.Sitemap.svg?style=flat-square)](https://www.nuget.org/packages/BlendInteractive.Umbraco.AltTextAi/)

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
dotnet add package BlendInteractive.Umbraco.AltTextAi
```

Or Nuget
```
Install-Package BlendInteractive.Umbraco.AltTextAi
```

## Configuration

First, in the Umbraco Backoffice, go to the setup tab and add a field to your image objects to 
hold your alt text. Take note of the property alias (we recommend calling the field "Alt Text", 
with an alias of "altText")

Next, you'll need to add a section to your appsettings.json file to configure the package.

```json
  "AltTextAi": {
    "ImageAltTextProperty": "altText",
    "AltTextAiApiKey": "<YOUR API KEY GOES HERE>",
    "AltTextLengthToSkip": 0,
    "AltTextKeyWords": []
  }
```

In the "ImageAltTextProperty" setting, add the alias for your alt text field you've just added. 
This is where the package will look for and write generated alt text.

In the "AltTextAiApiKey" setting, add an API Key from your AltTextAI account. You'll need 
an alttext.ai account for this. A free trial is available. 

The "AltTextLengthToSkip" setting allows you to skip generating alt text for media items where
the alt text is longer than the specified length. Set to 0 to skip generating alt text when the field is not empty.
This can be useful if you have manually added alt text to some media items and don't want to overwrite it.

The "AltTextKeyWords" setting allows you to specify keywords that will be used to give SEO context to
AltText.ai when generating the alt text. You can specify up to 6 keywords in the string array.

Once you have an account, the [AltText.AI documentation explains how to create an API Key](https://alttext.ai/docs/webui/account/#api-keys).

After this is completed, restart your Umbraco instance. Whenever an image is uploaded,
or an existing one without alt text is saved, you should see alt text added to the 
field you specified within a few seconds. 

You can edit the alt text manually if you wish. The AI will only generate text if 
the field is blank.

Note that each generation consumes a credit with the alttext.ai service!


