class ApiLinter < Formula
  desc "Linter for APIs defined in protocol buffers"
  homepage "https://linter.aip.dev/"
  url "https://github.com/googleapis/api-linter/archive/refs/tags/v2.4.1.tar.gz"
  sha256 "253dc16eac247991b2c27d37e044bba2f1c4040da4a20fc3e82af4e800149b91"
  license "Apache-2.0"
  head "https://github.com/googleapis/api-linter.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "60ece889af534e1d591a708cc99fecc4ad765855f653e1816c77f6f2b6974151"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/api-linter"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/api-linter --version")

    protofile = testpath/"proto3.proto"
    protofile.write <<~EOS
      syntax = "proto3";
      package proto3;

      message Request {
        string name = 1;
        repeated int64 key = 2;
      }
    EOS

    assert_match "message: Missing comment over \"Request\"", shell_output("#{bin}/api-linter proto3.proto 2>&1")
  end
end
