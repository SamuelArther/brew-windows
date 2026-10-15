class ProtocGenGo < Formula
  desc "Go support for Google's protocol buffers"
  homepage "https://github.com/protocolbuffers/protobuf-go"
  url "https://github.com/protocolbuffers/protobuf-go/releases/download/v1.36.12/protoc-gen-go.v1.36.12.windows.amd64.zip"
  sha256 "ab2aa2c53df000ad47c6a546e56556d9adb1767e38136f7c9a042e0a1c22793f"
  license "BSD-3-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/protocolbuffers/protobuf-go/archive/refs/tags/v1.36.12.tar.gz"
  end

  def install
    bin.install "protoc-gen-go.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/protoc-gen-go --version 2>&1")
  end
end
