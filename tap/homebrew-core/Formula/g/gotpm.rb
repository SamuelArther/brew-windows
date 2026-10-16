class Gotpm < Formula
  desc "CLI for using TPM 2.0"
  homepage "https://github.com/google/go-tpm-tools"
  url "https://github.com/google/go-tpm-tools/releases/download/v0.4.11/go-tpm-tools_Windows_x86_64.zip"
  sha256 "8f9a2219624a709867a554b6d765a7c85d6257cb85673e4fbf8457520e7b66b2"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/google/go-tpm-tools/archive/refs/tags/v0.4.11.tar.gz"
  end

  def install
    bin.install "gotpm.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gotpm --version 2>&1")
  end
end
