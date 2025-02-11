import React, { useState } from 'react';

const FileUpload: React.FC = () => {
    const [file, setFile] = useState<File | null>(null);
    const [errors, setErrors] = useState<string[]>([]);

    const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        if (file) {
            setFile(file);
            setErrors([]); // Очистити помилки при виборі нового файлу
        }
    };

    const handleSubmit = async (event: React.FormEvent) => {
        event.preventDefault();

        if (!file) {
            setErrors(['Будь ласка, виберіть файл!']);
            return;
        }

        if (file.type !== 'application/vnd.openxmlformats-officedocument.wordprocessingml.document') {
            setErrors(['Невірний формат файлу. Повинно бути .docx']);
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

            const text = await response.text();
            console.log("Відповідь сервера:", text);

            const result = JSON.parse(text);

            if (response.ok) {
                setErrors([]); // Якщо помилок немає, очистити список
            } else {
                setErrors(result.errors || ['Невідома помилка']);
            }
        } catch (error) {
            setErrors(["Помилка з'єднання з сервером."]);
        }
    };

    return (
        <div className="file-upload">
            <form onSubmit={handleSubmit}>
                <input type="file" onChange={handleFileChange} />
                <button type="submit">Завантажити файл</button>
            </form>
            {errors.length > 0 && (
                <div className="error-messages">
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


