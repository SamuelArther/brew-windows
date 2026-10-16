class ProtocGenJs < Formula
  desc "Protocol buffers JavaScript generator plugin"
  homepage "https://github.com/protocolbuffers/protobuf-javascript"
  url "https://github.com/protocolbuffers/protobuf-javascript/releases/download/v4.0.3/protobuf-javascript-4.0.3-win64.zip"
  sha256 "81be2b9a4016c9d2079c9c53d068b7d3c2a5fee4c958538b0214503b8aa1b4ae"
  license "BSD-3-Clause"
  revision 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/protocolbuffers/protobuf-javascript/archive/refs/tags/v4.0.3.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/bin/protoc-gen-js.exe", "protoc-gen-js"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/protoc-gen-js --version 2>&1")
  end
end
