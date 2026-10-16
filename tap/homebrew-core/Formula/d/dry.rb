class Dry < Formula
  desc "Terminal application to manage Docker and Docker Swarm"
  homepage "https://moncho.github.io/dry/"
  url "https://github.com/moncho/dry/releases/download/v0.13.0/dry-windows-amd64.zip"
  sha256 "1ec596e227f49011893a9e8b27eb285197da9567cc67312880dfd780ca0bce8e"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/moncho/dry/archive/refs/tags/v0.13.0.tar.gz"
  end

  def install
    bin.install "dry.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dry --version")
    assert_match "A tool to interact with a Docker Daemon from the terminal", shell_output("#{bin}/dry --description")
  end
end
