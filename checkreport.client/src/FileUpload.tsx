import React, { useState } from 'react';

const FileUpload: React.FC = () => {
    const [file, setFile] = useState<File | null>(null);
    const [errors, setErrors] = useState<string[]>([]);
    const [message, setMessage] = useState<string>("");

    const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        const selectedFile = event.target.files?.[0];
        if (selectedFile) {
            if (selectedFile.type !== "application/pdf") {
                setErrors(["Невірний формат файлу. Завантажуйте тільки PDF."]);
                setFile(null);
                return;
            }
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

            // Отримуємо текст відповіді
            const text = await response.text();
            console.log("Відповідь сервера:", text);

            // Спробуємо розпарсити JSON
            try {
                const result = JSON.parse(text);

                if (response.ok) {
                    setMessage(result.message);
                    setErrors([]);
                } else {
                    setErrors(result.errors || ["Невідома помилка"]);
                    setMessage("");
                }
            } catch (error) {
                setErrors(["Помилка обробки відповіді від сервера."]);
                setMessage("");
            }

        } catch (error) {
            setErrors(["Помилка з'єднання з сервером."]);
            setMessage("");
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

