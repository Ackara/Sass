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

    let cssFile = args.getOutputPath(true);
    let mapFile = args.getSourceMapPath(true);
    let options = {
        outFile: cssFile,
        sourceMap: mapFile,
        file: args.sourceFile,
        sourceComments: args.addSourceComments,
        omitSourceMapUrl: (args.generateSourceMaps == false),
        outputStyle: "expanded"
    };

    sass.render(options, function (error, sassResult) {
        if (error) {
            console.error(JSON.stringify({
                file: error.file,
                line: error.line,
                column: error.column,
                message: error.message,
                status: ("SASS" + error.status)
            }));
            return;
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

function CompilerOptions() {
    let me = this;
    let bool = /true/i;

    me.sourceFile = process.argv[2];

    me.outputDirectory = process.argv[3];
    if (!me.outputDirectory) { me.outputDirectory = path.dirname(me.sourceFile); }

    me.sourceMapDirectory = process.argv[4];
    if (!me.sourceMapDirectory) { me.sourceMapDirectory = me.outputDirectory; }

    me.suffix = process.argv[5];
    me.minify = bool.test(process.argv[6]);

    me.keepIntermediateFiles = bool.test(process.argv[7]);
    me.generateSourceMaps = bool.test(process.argv[8]);
    me.addSourceComments = bool.test(process.argv[9]);

    me.getOutputPath = function (omitSuffix = false) {
        let baseName = path.basename(me.sourceFile, path.extname(me.sourceFile));
        return path.join(me.outputDirectory, (baseName + (omitSuffix ? "" : me.suffix) + ".css"));
    }

    me.getSourceMapPath = function (omitSuffix = false) {
        let baseName = path.basename(me.sourceFile, path.extname(me.sourceFile));
        return path.join(me.sourceMapDirectory, (baseName + (omitSuffix ? "" : me.suffix) + ".css.map"));
    }

    me.log = function () {
        console.log("src: " + me.sourceFile);
        console.log("out: " + me.getOutputPath());
        console.log("dir: " + me.outputDirectory);
        console.log("smd: " + me.sourceMapDirectory);
        console.log("suf: " + me.suffix);

        console.log("min: " + me.minify);
        console.log("map: " + me.generateSourceMaps);
        console.log("imm: " + me.keepIntermediateFiles);
        console.log("====================");
        console.log("");
    }
    me.log();
}

// Main
// ==================================================

compile(new CompilerOptions());