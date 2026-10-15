class Peco < Formula
  desc "Simplistic interactive filtering tool"
  homepage "https://github.com/peco/peco"
  url "https://github.com/peco/peco/releases/download/v0.6.0/peco_0.6.0_windows_amd64.zip"
  sha256 "5bd9aa4bc02338a6ec8ab1ef212c7b2e54c23e2416254c7681f03e9fa6857863"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/peco/peco/archive/refs/tags/v0.6.0.tar.gz"
  end

  def install
    bin.install "peco.exe"
  end

  test do
    system bin/"peco", "--version"

    ENV["TERM"] = "xterm"
    assert_match "homebrew", pipe_output("#{bin}/peco --select-1", "homebrew\n", 0)
  end
end
