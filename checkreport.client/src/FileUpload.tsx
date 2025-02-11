import React, { useState } from 'react';

const FileUpload: React.FC = () => {
    const [file, setFile] = useState<File | null>(null);
    const [errors, setErrors] = useState<string[]>([]);
    const [message, setMessage] = useState<string>("");

    const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        const selectedFile = event.target.files?.[0];
        if (selectedFile) {
            setFile(selectedFile);
            setErrors([]);
            setMessage("");
        }
    };

    const handleSubmit = async (event: React.FormEvent) => {
        event.preventDefault();
        if (!file) {
            setErrors(["Будь ласка, виберіть файл!"]);
            return;
        }

        const formData = new FormData();
        formData.append('file', file);

        try {
            const response = await fetch('https://localhost:7270/api/validate', {
                method: 'POST',
                body: formData,
                headers: { "Accept": "application/json" }
            });

            const result = await response.json();
            if (response.ok) {
                setMessage(result.message);
                setErrors([]);
            } else {
                setErrors(result.errors || ["Невідома помилка"]);
            }
        } catch (error) {
            setErrors(["Помилка з'єднання з сервером."]);
        }
    };

    return (
        <div>
            <form onSubmit={handleSubmit}>
                <input type="file" onChange={handleFileChange} />
                <button type="submit">Перевірити</button>
            </form>

            {message && <p style={{ color: "green" }}>{message}</p>}
            {errors.length > 0 && (
                <div style={{ color: "red" }}>
                    <ul>
                        {errors.map((error, index) => (
                            <li key={index}>{error}</li>
                        ))}
                    </ul>
                </div>
            )}
        </div>
    );
};

export default FileUpload;
