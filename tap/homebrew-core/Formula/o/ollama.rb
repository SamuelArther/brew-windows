class Ollama < Formula
  desc "Create, run, and share large language models (LLMs)"
  homepage "https://ollama.com/"
  url "https://github.com/ollama/ollama/releases/download/v0.40.4/ollama-windows-amd64.zip"
  sha256 "7485a1172f8527db4342a63521b9342fc466185ff0855da378cc864ecf6354cf"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/ollama.exe", "ollama"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ollama --version 2>&1")
  end
end
