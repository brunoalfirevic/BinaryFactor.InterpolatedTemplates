param(
    [string] [Parameter(Position = 0)] $command,
    [string[]] [Parameter(Position = 1, ValueFromRemainingArguments)] $arguments = @())

. .\go-helpers

$dotnet_full_version = "net461"
$dotnet_core_version = "netcoreapp3.0"

$main_project = "Relator"
$test_project = "Relator.Tests"

function clean {
    info "Cleaning build artifacts"
    exec {dotnet clean src -v minimal /nologo}
}

function build {
    info "Building solution"
    exec {dotnet build src /nologo}
}

function rebuild {
    clean
    build
}

function test {
    info "Running tests using .NET Core"
    exec {dotnet run --project "src/$test_project" --framework $dotnet_core_version}
}

function watch-test {
    exec {dotnet watch --project "src/$test_project" run}
}

function watch-build {
    exec {dotnet watch --project "src/$test_project" build /nologo}
}

function test-full {
    info "Running tests using .NET Full Framework"
    exec {dotnet run --project "src/$test_project" --framework $dotnet_full_version}
}

function publish {
    info "Deleting 'publish' folder"
    remove-folder "./publish"

    info "Publishing to 'publish' folder"
    exec {dotnet publish "src/$main_project" -c Release --self-contained true -o ./publish/RelatorNet /nologo}
}

function go {
    rebuild
    test
    test-full
}

main {
    if ($command) {
        & $command
    } else {
        info  @"
Available commands:
    go

    clean
    build
    rebuild

    test
    test-full
    watch-test
    watch-build
"@
    }
}
