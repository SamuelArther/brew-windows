class Yamlfmt < Formula
  desc "Extensible command-line tool to format YAML files"
  homepage "https://github.com/google/yamlfmt"
  url "https://github.com/google/yamlfmt/releases/download/v0.21.0/yamlfmt_0.21.0_Windows_x86_64.tar.gz"
  sha256 "07f80ce5d741eb4b0a9380ac78a19c7cb5bd44e2a9a47a5a04839e3ba54dd463"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/google/yamlfmt/archive/refs/tags/v0.21.0.tar.gz"
  end

  def install
    bin.install "yamlfmt.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/yamlfmt --version 2>&1")
  end
end
