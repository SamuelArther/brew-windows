class ProtocGenDoc < Formula
  desc "Documentation generator plugin for Google Protocol Buffers"
  homepage "https://github.com/pseudomuto/protoc-gen-doc"
  url "https://github.com/pseudomuto/protoc-gen-doc/releases/download/v1.5.1/protoc-gen-doc_1.5.1_windows_amd64.tar.gz"
  sha256 "8acf0bf64eda29183b4c6745c3c6a12562fd9a8ab08d61788cf56e6659c66b3b"
  license "MIT"
  revision 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/pseudomuto/protoc-gen-doc/archive/refs/tags/v1.5.1.tar.gz"
  end

  def install
    bin.install "protoc-gen-doc.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/protoc-gen-doc --version 2>&1")
  end
end
