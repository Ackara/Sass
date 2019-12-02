// Using
// ==================================================
const os = require("os");
const fs = require("fs");
const path = require("path");
const csso = require("csso");
const sass = require("node-sass");
const sourceMapMerger = require("multi-stage-sourcemap").transfer;

// Methods
// ==================================================

function compile(args) {
    let generatedFiles = [];
    let cssFile = args.getOutputPath();
    let mapFile = args.getSourceMapPath();

    args.sass.outFile = cssFile;
    args.sass.sourceMap = mapFile;
    args.sass.file = args.sourceFile;

    sass.render(args.sass, function (err, sassResult) {
        if (err) {
            throw JSON.stringify({
                file: err.file,
                line: err.line,
                column: err.column,
                message: err.message,
                status: err.status
            });
        }

        if (args.minify) {
            let minifiedResult = minify(path.relative(args.sourceMapDirectory, cssFile), sassResult.css.toString(), args);
            let minifiedCss = minifiedResult.css.toString();
            let minifiedFile = args.getOutputPath();

            if (args.generateSourceMaps) {
                let finalMapFile = args.getSourceMapPath();
                let mergedMap = mergeSourceMaps(
                    minifiedResult.map.toString(),
                    sassResult.map.toString()
                );
                createFile(finalMapFile, JSON.stringify(mergedMap, null, 2));
                generatedFiles.push(finalMapFile);

                minifiedCss = (minifiedCss + os.EOL + "/*# sourceMappingURL=" + path.relative(args.outputDirectory, finalMapFile) + " */");
            }

            createFile(minifiedFile, minifiedCss, args);
            generatedFiles.push(minifiedFile);
        }
        else {
            if (args.generateSourceMaps) {
                createFile(mapFile, sassResult.map);
                generatedFiles.push(mapFile);
            }

            createFile(cssFile, sassResult.css);
            generatedFiles.push(cssFile);
        }

        console.log(JSON.stringify(generatedFiles));
    });
}

function minify(cssFile, content, args) {
    return csso.minify(content, {
        filename: cssFile,
        sourceMap: args.generateSourceMaps
    });
}

function mergeSourceMaps(mapB, mapA) {
    var result = sourceMapMerger({ fromSourceMap: mapB, toSourceMap: mapA });
    return JSON.parse(result.toString());
}

function createFile(absoluePath, content) {
    fs.writeFile(absoluePath, content, function (ex) {
        if (ex) { console.error(ex.message); }
    });
}

function mergeOptions(options) {
    var config = { sass: null };
    if (options.optionsFile) {
        config = JSON.parse(fs.readFileSync(options.optionsFile, "uft8"));
    }

    var sassOptions = (config.sass ? config.sass : {});
    if (!sassOptions.hasOwnProperty("outputStyle")) { sassOptions.outputStyle = "expanded"; }
    if (!sassOptions.hasOwnProperty("sourceComments")) { sassOptions.sourceComments = options.addSourceComments; }
    if (!sassOptions.hasOwnProperty("omitSourceMapUrl")) { sassOptions.omitSourceMapUrl = (options.generateSourceMaps == false); }

    if (!sassOptions.functions) { sassOptions.functions = {}; }
    if (!sassOptions.functions.hasOwnProperty("@debug")) {
        sassOptions.functions["@debug"] = function (msg) {
            console.error(JSON.stringify({
                message: ("debug: " + msg.getValue()),
                file: options.sourceFile,
                level: 0
            }));
            return sass.types.Null;
        }
    }
    if (!sassOptions.functions.hasOwnProperty("@warn")) {
        sassOptions.functions["@warn"] = function (msg) {
            console.error(JSON.stringify({
                message: msg.getValue(),
                file: options.sourceFile,
                level: 1
            }));
            return sass.types.Null;
        }
    }
    if (!sassOptions.functions.hasOwnProperty("@error")) {
        sassOptions.functions["@error"] = function (msg) {
            console.error(JSON.stringify({
                message: msg.getValue(),
                file: options.sourceFile,
                level: 2
            }));
            return sass.types.Null;
        }
    }
    options.sass = sassOptions;
}

function CompilerOptions() {
    let me = this;
    let bool = /true/i;

    me.sourceFile = process.argv[2];
    me.optionsFile = process.argv[3];

    me.outputDirectory = process.argv[4];
    if (!me.outputDirectory) { me.outputDirectory = path.dirname(me.sourceFile); }

    me.sourceMapDirectory = process.argv[5];
    if (!me.sourceMapDirectory) { me.sourceMapDirectory = me.outputDirectory; }

    me.minify = bool.test(process.argv[6]);
    me.generateSourceMaps = bool.test(process.argv[7]);
    me.addSourceComments = bool.test(process.argv[8]);

    mergeOptions(me);

    me.getOutputPath = function () {
        let baseName = path.basename(me.sourceFile, path.extname(me.sourceFile));
        return path.join(me.outputDirectory, (baseName + ".css"));
    }

    me.getSourceMapPath = function () {
        let baseName = path.basename(me.sourceFile, path.extname(me.sourceFile));
        return path.join(me.sourceMapDirectory, (baseName + ".css.map"));
    }

    me.log = function () {
        console.log("src: " + me.sourceFile);
        console.log("out: " + me.getOutputPath());
        console.log("dir: " + me.outputDirectory);
        console.log("smd: " + me.sourceMapDirectory);

        console.log("min: " + me.minify);
        console.log("com: " + me.addSourceComments);
        console.log("map: " + me.generateSourceMaps);
        console.log("====================");
        console.log("");
    }
    me.log();
}

// Main
// ==================================================

compile(new CompilerOptions());