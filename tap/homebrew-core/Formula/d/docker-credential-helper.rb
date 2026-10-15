class DockerCredentialHelper < Formula
  desc "Platform keystore credential helper for Docker"
  homepage "https://github.com/docker/docker-credential-helpers"
  url "https://github.com/docker/docker-credential-helpers/releases/download/v0.9.10/docker-credential-wincred-v0.9.10.windows-amd64.exe"
  sha256 "04833862d29a06a6f9353ef0619b7385f37ee3ce1650de5ad3ebe9c3f2ebe233"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/docker/docker-credential-helpers/archive/refs/tags/v0.9.10.tar.gz"
  end

  def install
    bin.install "docker-credential-wincred-v0.9.10.windows-amd64.exe" => "docker-credential-wincred.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/docker-credential-wincred --version 2>&1")
  end
end
