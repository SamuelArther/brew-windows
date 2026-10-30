class Thriftgo < Formula
  desc "Implementation of thrift compiler in go language with plugin mechanism"
  homepage "https://github.com/cloudwego/thriftgo"
  url "https://github.com/cloudwego/thriftgo/archive/refs/tags/v0.4.5.tar.gz"
  sha256 "8469be2df13bb1c2128a3594e4d59fb718ddf3da603181d163dbcf3d230214af"
  license "Apache-2.0"
  compatibility_version 1
  head "https://github.com/cloudwego/thriftgo.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "52da41c30a67e17ba1b0f84c1cf3d78451dc29498589718ba90dc8243f47edee"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    output = shell_output("#{bin}/thriftgo --version 2>&1")
    assert_match "thriftgo #{version}", output

    thriftfile = testpath/"test.thrift"
    thriftfile.write <<~THRIFT
      namespace go api
      struct Request {
              1: string message
      }
      struct Response {
              1: string message
      }
      service Hello {
          Response echo(1: Request req)
      }
    THRIFT
    system bin/"thriftgo", "-o=.", "-g=go", "test.thrift"
    assert_path_exists testpath/"api/test.go"
    refute_predicate (testpath/"api/test.go").size, :zero?
  end
end
