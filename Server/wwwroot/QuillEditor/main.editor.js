import Quill from "quill";
// import BlotFormatter from "@enzedonline/quill-blot-formatter2";

let quillInstances = new Map();

export function initQuill(elementId, dotNetRef) {
	const quill = new Quill(`#${elementId}`, {
		modules: {
			toolbar: {
				container: [
                    [{ font: [] }, { size: [] }],
                    [{ header: [1, 2, 3, 4, 5, 6 ]}],
					['bold', 'italic', 'underline', 'strike'],
                    [{ color: [] }, { background: [] }],
                    [{ list: 'ordered' }, { list: 'bullet' }, { list: 'check' }],
					['blockquote', 'code-block'],
                    ['link', 'image'],
                    [{ align: [] }, { direction: 'rtl' }]
				],
                
				handlers: {
					image: () => dotNetRef.invokeMethodAsync("OpenImageSelector")
				}
			}
		},
		placeholder: "Börja skapa innehåll här...",
		theme: "snow"
	});

	quillInstances.set(elementId, quill);

	quill.on("text-change", () => {
		const delta = JSON.stringify(quill.getContents());
		dotNetRef.invokeMethodAsync("UpdateDeltaJSONContent", delta);
	});
}

export function getDeltaJSONContent(elementId) {
	const quill = quillInstances.get(elementId);
	return quill ? JSON.stringify(quill.getContents()) : "Quill instance not initialized.";
}

export function setDeltaJSONContent(elementId, deltaJSON) {
	const quill = quillInstances.get(elementId);
	if (quill) {
		const delta = typeof deltaJSON === "string" ? JSON.parse(deltaJSON) : deltaJSON;
        quill.setContents(delta, 'silent');
	}
	return quill === undefined ? "Quill instance not initialized." : "Content set successfully.";
}

export function insertImage(elementId, imageId) {
    const quill = quillInstances.get(elementId);
    if (quill) {
        const range = quill.getSelection();
        const imageUrl = `/images/${imageId}/jpg/desktop.jpg`
        quill.insertEmbed(range ? range.index : 0, 'image', imageUrl);
    }
}