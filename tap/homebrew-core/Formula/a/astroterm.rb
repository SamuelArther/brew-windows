class Astroterm < Formula
  desc "Planetarium for your terminal"
  homepage "https://github.com/da-luce/astroterm"
  url "https://github.com/da-luce/astroterm/releases/download/v1.2.0/astroterm-win-x86_64.exe"
  sha256 "397b3b9d61ae1d744cbcd2906985e8fcfd5dc13f3b2391ee56fda1c589b080b4"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/da-luce/astroterm/archive/refs/tags/v1.2.0.tar.gz"
  end

  def install
    bin.install "astroterm-win-x86_64.exe" => "astroterm.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/astroterm --version 2>&1")
  end
end
