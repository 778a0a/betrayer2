mergeInto(LibraryManager.library, {
    ShowSaveDataTextArea: function (saveDataPtr) {
        var saveData = UTF8ToString(saveDataPtr);

        // オーバーレイを作成
        var overlay = document.createElement('div');
        overlay.id = 'saveDataOverlay';
        overlay.style.cssText = 'position: fixed; top: 0; left: 0; right: 0; bottom: 0; background-color: rgba(0, 0, 0, 0.5); z-index: 10000; display: flex; align-items: center; justify-content: center;';

        // コンテナを作成
        var container = document.createElement('div');
        container.style.cssText = 'background-color: #fff; border: 1px solid #ccc; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1); padding: 20px; width: 80%; max-width: 1000px; max-height: 80vh; display: flex; flex-direction: column;';

        // タイトルバーを作成
        var titleBar = document.createElement('div');
        titleBar.style.cssText = 'display: flex; justify-content: space-between; align-items: center; margin-bottom: 15px;';

        var title = document.createElement('div');
        title.textContent = '以下のテキストをコピーして保存してください';
        title.style.cssText = 'color: #333; font-size: 18px; font-weight: bold;';

        var closeButton = document.createElement('button');
        closeButton.textContent = '×';
        closeButton.style.cssText = 'font-size: 24px; width: 40px; height: 40px; cursor: pointer; padding: 0;';
        closeButton.onclick = function() {
            document.body.removeChild(overlay);
        };

        titleBar.appendChild(title);
        titleBar.appendChild(closeButton);

        // テキストエリアを作成
        var textarea = document.createElement('textarea');
        textarea.value = saveData;
        textarea.readOnly = true;
        textarea.style.cssText = 'width: 100%; height: 500px; font-family: monospace; font-size: 13px; padding: 10px; resize: vertical; margin-bottom: 15px; background-color: #f9f9f9; color: #333; border: 1px solid #ddd; border-radius: 4px; box-sizing: border-box;';

        // ボタンコンテナを作成
        var buttonContainer = document.createElement('div');
        buttonContainer.style.cssText = 'display: flex; gap: 10px;';

        // コピーボタンを作成
        var copyButton = document.createElement('button');
        copyButton.textContent = 'クリップボードにコピー';
        copyButton.style.cssText = 'flex: 1; padding: 12px; font-size: 16px; cursor: pointer;';
        copyButton.onclick = function() {
            textarea.select();
            textarea.setSelectionRange(0, textarea.value.length); // 実際の文字数を使用

            try {
                // navigator.clipboard APIを使用（より新しい方法）
                if (navigator.clipboard && navigator.clipboard.writeText) {
                    navigator.clipboard.writeText(saveData).then(function() {
                        var originalText = copyButton.textContent;
                        copyButton.textContent = 'コピーしました！';
                        setTimeout(function() {
                            copyButton.textContent = originalText;
                        }, 2000);
                    }).catch(function(err) {
                        console.error('コピーに失敗しました:', err);
                        // フォールバック: execCommandを試す
                        fallbackCopy();
                    });
                } else {
                    // 古いブラウザ対応
                    fallbackCopy();
                }
            } catch (err) {
                console.error('コピーに失敗しました:', err);
                fallbackCopy();
            }

            function fallbackCopy() {
                try {
                    document.execCommand('copy');
                    var originalText = copyButton.textContent;
                    copyButton.textContent = 'コピーしました！';
                    setTimeout(function() {
                        copyButton.textContent = originalText;
                    }, 2000);
                } catch (err) {
                    console.error('フォールバックコピーも失敗しました:', err);
                    alert('コピーに失敗しました。手動で選択してコピーしてください。');
                }
            }
        };

        // 閉じるボタンを作成
        var closeButton2 = document.createElement('button');
        closeButton2.textContent = '閉じる';
        closeButton2.style.cssText = 'flex: 1; padding: 12px; font-size: 16px; cursor: pointer;';
        closeButton2.onclick = function() {
            document.body.removeChild(overlay);
        };

        buttonContainer.appendChild(copyButton);
        buttonContainer.appendChild(closeButton2);

        // すべてを組み立てる
        container.appendChild(titleBar);
        container.appendChild(textarea);
        container.appendChild(buttonContainer);
        overlay.appendChild(container);
        document.body.appendChild(overlay);

        // テキストエリアをフォーカスして全選択
        textarea.focus();
        textarea.select();
    }
});
