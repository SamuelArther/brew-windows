class RapidMlx < Formula
  desc "Fast local AI engine with an OpenAI-compatible API, using llama.cpp"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "rapid-mlx", "rapid-mlx"
  end

  def caveats
    <<~EOS
      rapid-mlx serves models with llama.cpp's llama-server. Install the engine with:
        brew install llama.cpp
      Then start a server on http://127.0.0.1:8000/v1 with:
        rapid-mlx serve qwen3.5-4b-4bit
    EOS
  end

  test do
    assert_match(/rapid-mlx \d+\.\d+\.\d+/, shell_output("#{bin}/rapid-mlx --version"))
    assert_match "qwen3.5-4b-4bit", shell_output("#{bin}/rapid-mlx info qwen3.5-4b-4bit")
    assert_match "gpt-oss-20b-4bit", shell_output("#{bin}/rapid-mlx models")
    assert_match "No models downloaded", shell_output("#{bin}/rapid-mlx ls")
    assert_match "unknown model", shell_output("#{bin}/rapid-mlx info no-such-model 2>&1", 2)
  end
end
