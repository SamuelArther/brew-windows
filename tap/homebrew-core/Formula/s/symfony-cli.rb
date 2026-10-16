class SymfonyCli < Formula
  desc "Build, run, and manage Symfony applications"
  homepage "https://symfony.com/download"
  url "https://github.com/symfony-cli/symfony-cli/releases/download/v5.22.1/symfony-cli_windows_amd64.zip"
  sha256 "b67c87566d5ca051f5b4fadcd8a627f258378d531f60b156c7094f3136676d09"
  license "AGPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/symfony-cli/symfony-cli/archive/refs/tags/v5.22.1.tar.gz"
  end

  def install
    bin.install "symfony.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/symfony -V 2>&1")
  end
end
