#!/bin/bash

rm src/Redis.OM.Vectorizers.AllMiniLML6V2/Resources/model.onnx
curl -o ./src/Redis.OM.Vectorizers.AllMiniLML6V2/Resources/model.onnx https://storage.googleapis.com/slorello/model.onnx

rm src/Redis.OM.Vectorizers.AllMiniLML6V2/Resources/vocab.txt
curl -o ./src/Redis.OM.Vectorizers.AllMiniLML6V2/Resources/vocab.txt https://storage.googleapis.com/slorello/vocab.txt

rm src/Redis.OM.Vectorizers.Resnet18/Resources/ResNet18Onnx/ResNet18.onnx 
curl -o ./src/Redis.OM.Vectorizers.Resnet18/Resources/ResNet18Onnx/ResNet18.onnx  https://storage.googleapis.com/slorello/ResNet18.onnx

rm src/Redis.OM.Vectorizers.Resnet18/Resources/ResNet18Onnx/ResNetPreprocess.onnx 
curl -o ./src/Redis.OM.Vectorizers.Resnet18/Resources/ResNetPrepOnnx/ResNetPreprocess.onnx  https://storage.googleapis.com/slorello/ResNetPreprocess.onnx