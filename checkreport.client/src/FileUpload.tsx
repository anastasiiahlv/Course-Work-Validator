import React, { useState } from 'react';

const FileUpload: React.FC = () => {
    const [file, setFile] = useState<File | null>(null);
    const [message, setMessage] = useState<string>('');

    const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        if (file) {
            setFile(file);
            setMessage('');
        }
    };

    const handleSubmit = async (event: React.FormEvent) => {
        event.preventDefault();

        if (!file) {
            setMessage('Будь ласка, виберіть файл!');
            return;
        }

        if (file.type !== 'application/vnd.openxmlformats-officedocument.wordprocessingml.document') {
            setMessage('Невірний формат файлу. Повинно бути .docx');
            return;
        }

        // Формуємо FormData і відправляємо на сервер
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
            setMessage(`Результат: ${result.message}`);
        } catch (error) {
            setMessage("Помилка з'єднання з сервером.");
        }
    };

    return (
        <div>
            <form onSubmit={handleSubmit}>
                <input type="file" onChange={handleFileChange} />
                <button type="submit">Завантажити файл</button>
            </form>
            <div>{message}</div>
        </div>
    );
};

export default FileUpload;
