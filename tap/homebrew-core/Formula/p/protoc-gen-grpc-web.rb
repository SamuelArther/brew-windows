class ProtocGenGrpcWeb < Formula
  desc "Protoc plugin that generates code for gRPC-Web clients"
  homepage "https://grpc.io"
  url "https://github.com/grpc/grpc-web/releases/download/2.1.1/protoc-gen-grpc-web-2.1.1-windows-x86_64.exe"
  sha256 "206fde8d99a75a1586fe13164ce0e7497afd358c0cf47c75dbebf3423dd35873"
  license "Apache-2.0"
  revision 2

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/grpc/grpc-web/archive/refs/tags/2.1.1.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "protoc-gen-grpc-web-2.1.1-windows-x86_64.exe" => "protoc-gen-grpc-web.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/protoc-gen-grpc-web --version 2>&1")
  end
end
