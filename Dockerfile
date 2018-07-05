FROM registry.valtech.dk/buildtools@sha256:168e154d20bfd7ade9c7d3bf30ff2fd8aa46e25bcceabde382df84a6b5780ea8

# Ensure that we only run NuGet if any packages has changed
WORKDIR C:/workspace/
COPY nuget.config .
COPY *.sln .
COPY ./src/Feature/packages.config ./src/Feature/packages.config
COPY ./src/Feature/PhillipsMedisize.Feature.csproj ./src/Feature/PhillipsMedisize.Feature.csproj
COPY ./src/Foundation/packages.config ./src/Foundation/packages.config
COPY ./src/Foundation/PhillipsMedisize.Foundation.csproj ./src/Foundation/PhillipsMedisize.Foundation.csproj
COPY ./src/Project/CorporateWebsite/packages.config ./src/Project/CorporateWebsite/packages.config
COPY ./src/Project/CorporateWebsite/PhillipsMedisize.CorporateWebsite.csproj ./src/Project/CorporateWebsite/PhillipsMedisize.CorporateWebsite.csproj
COPY ./test/PhilipsMedisize.Test/packages.config ./test/PhilipsMedisize.Test/packages.config
COPY ./test/PhilipsMedisize.Test/PhilipsMedisize.Test.csproj ./test/PhilipsMedisize.Test/PhilipsMedisize.Test.csproj
RUN nuget restore

# Ensure that we only run NPM if any packages has changed
COPY ./src/Project/CorporateWebsite/package-lock.json ./src/Project/CorporateWebsite/package-lock.json
COPY ./src/Project/CorporateWebsite/package.json ./src/Project/CorporateWebsite/package.json
WORKDIR C:/workspace/src/Project/CorporateWebsite
RUN npm install
RUN npm install gulp-cli -g

# Copy everything from the context
WORKDIR C:/workspace/
COPY . .

# Build and publish solution
RUN msbuild /m /v:m /p:Configuration=Release /p:DeployOnBuild=True /p:DeployDefaultTarget=WebPublish /p:WebPublishMethod=FileSystem /p:PublishUrl=c:\out\Website

# Copy serialized items
RUN (robocopy /E /NP /NJH /NJS .\src\Items c:\out\Website\App_Data\serialization) ^& IF %ERRORLEVEL% LEQ 1 exit 0

# Build the frontend
WORKDIR C:/workspace/src/Project/CorporateWebsite
RUN gulp --prod
RUN (robocopy /E /NP /NJH /NJS .\dist c:\out\Website\dist) ^& IF %ERRORLEVEL% LEQ 1 exit 0