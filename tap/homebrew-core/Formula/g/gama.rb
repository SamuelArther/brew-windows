class Gama < Formula
  desc "Manage your GitHub Actions from Terminal with great UI"
  homepage "https://github.com/termkit/gama"
  url "https://github.com/termkit/gama/releases/download/v1.2.1/gama-windows-amd64.exe"
  sha256 "386f11124c755b571e68f1c19e47aed0dc128eb788d136d295a02bdd3cd108b6"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/termkit/gama/archive/refs/tags/v1.2.1.tar.gz"
  end

  def install
    bin.install "gama-windows-amd64.exe" => "gama.exe"
  end

  test do
    # gama is a TUI app
    #
    # There's no any output to stdout (except for ncurses-like UI) or a file
    # `gama --version` or `gama --help` are not valid options either
    pid = spawn bin/"gama"
    sleep 2
  ensure
    Process.kill("TERM", pid)
    Process.wait pid
  end
end
