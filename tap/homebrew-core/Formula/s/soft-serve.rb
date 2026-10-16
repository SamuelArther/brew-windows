class SoftServe < Formula
  desc "Mighty, self-hostable Git server for the command-line"
  homepage "https://github.com/charmbracelet/soft-serve"
  url "https://github.com/charmbracelet/soft-serve/releases/download/v0.12.3/soft-serve_0.12.3_Windows_x86_64.zip"
  sha256 "8b4df904351d3571029d8d1b71318f88c4569c84967f408f083f6690c4e49f2a"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/charmbracelet/soft-serve/releases/download/v0.12.3/soft-serve-0.12.3.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/soft.exe", "soft"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/soft --version")

    pid = spawn bin/"soft", "serve"
    sleep 1
    Process.kill("TERM", pid)
    assert_path_exists testpath/"data/soft-serve.db"
    assert_path_exists testpath/"data/hooks/update.sample"
  end
end
