import React from 'react';
import FileUpload from './FileUpload';

const App: React.FC = () => {
    return (
        <div style={{ textAlign: 'center', padding: '20px' }}>
            <h1>Перевірка курсових робіт</h1>
            <p>
                Завантажте документ у форматі .docx для перевірки на відповідність вимогам до
                оформлення курсових робіт.
            </p>
            <FileUpload />
        </div>
    );
};

export default App;
