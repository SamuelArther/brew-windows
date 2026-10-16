class Apfel < Formula
  desc "On-device LLM from the command-line, with OpenAI-compatible API server (llama.cpp)"
  homepage "https://github.com/ggml-org/llama.cpp"
  url "https://github.com/ggml-org/llama.cpp/releases/download/b11669/llama-b11669-bin-win-cpu-x64.zip"
  version "0.7.0"
  sha256 "37b3efcf962847ca869745fd4ccf7f5e7e8dacfeff9fa3523b4ca8cdc66d632b"
  license "MIT"

  # apfel drives Apple Intelligence's on-device model through FoundationModels, which exists only
  # on macOS 26. Windows has no built-in model a command-line tool can use, so this is the
  # standard local-LLM runtime there: the official ggml-org llama.cpp CPU build of v0.7.0 (build
  # b11669), preset to a small instruction model (Gemma 3 1B) that downloads on first use.
  #   apfel                 chat (llama-cli); -p "question" -st answers once and exits
  #   apfel-server          OpenAI-compatible API server (llama-server, /v1/chat/completions)
  # Any other -hf <user>/<model> or -m <file.gguf> picks a different model.

  livecheck do
    url "https://github.com/ggml-org/llama.cpp"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/llama-cli.exe", "apfel", "-hf", "ggml-org/gemma-3-1b-it-GGUF"
    windows_exe "libexec/llama-server.exe", "apfel-server", "-hf", "ggml-org/gemma-3-1b-it-GGUF"
  end

  def caveats
    <<~EOS
      apfel runs llama.cpp with Gemma 3 1B (about 800 MB, downloaded from Hugging Face on first
      use) instead of Apple Intelligence, and takes llama.cpp's options:
        apfel -p "What is the capital of Austria?" -st
        apfel-server --port 11434
      Pass -hf <user>/<model> or -m <file.gguf> to use another model.
    EOS
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/apfel --version 2>&1")
    output = shell_output("#{bin}/apfel-server --help 2>&1")
    assert_match "--port", output
    assert_match "--hf-repo", output
  end
end
