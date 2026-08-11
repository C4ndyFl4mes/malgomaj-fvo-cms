import { defineConfig } from 'vite';
import path from "node:path";

export default defineConfig({
    build: {
        emptyOutdir: false,
        outDir: path.resolve(__dirname, "Server/wwwroot/js"),
        lib: {
            entry: {
                editor: path.resolve(__dirname, "Server/wwwroot/QuillEditor/main.editor.js"),
                // treeviewer: path.resolve(__dirname, "Server/wwwroot/SortableJSTreeViewer/main.treeviewer.js")
            },
            formats: ["es"],
            fileName: (format, entryName) => `${entryName}.bundle.js`
        },
        rolldownOptions: {
            output: {
                entryFileNames: "[name].bundle.js",
                chunkFileNames: "[name].bundle.js",
                assetFileNames: "[name].[ext]"
            }
        }
    }
});