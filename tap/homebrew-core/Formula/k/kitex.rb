class Kitex < Formula
  desc "Golang RPC framework for microservices"
  homepage "https://www.cloudwego.io/docs/kitex/"
  license "Apache-2.0"
  head "https://github.com/cloudwego/kitex.git", branch: "main"

  stable do
    url "https://github.com/cloudwego/kitex/archive/refs/tags/v0.16.4.tar.gz"
    sha256 "db369c6387af3d29e1037adf31edf556356502b1e406bc056b448c24acce18fa"

    # Fix the reported version, upstream PR ref, https://github.com/cloudwego/kitex/pull/2009
    patch do
      url "https://github.com/chenrui333/kitex/commit/e136273bd4fce56882cc3f59efa5cbf55170892f.patch?full_index=1"
      sha256 "17bb2821c34139f8111e921b261f696d80aab95888abc44beee3158c59aee9d9"
      type :unofficial
      resolves "https://github.com/cloudwego/kitex/pull/2009"
    end
  end

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "dba57f9b2c0c4b164dc8a2831a3e0df9d7b4e7cea7c6f9b4db6bbfd8edf0060e"
  end

  depends_on "go" => [:build, :test]
  depends_on "thriftgo" => :test

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./tool/cmd/kitex"
  end

  test do
    assert_match "v#{version}", shell_output("#{bin}/kitex --version 2>&1")

    thriftfile = testpath/"test.thrift"
    thriftfile.write <<~EOS
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
    EOS
    system bin/"kitex", "-module", "test", "test.thrift"
    assert_path_exists testpath/"go.mod"
    refute_predicate (testpath/"go.mod").size, :zero?
    assert_path_exists testpath/"kitex_gen/api/test.go"
    refute_predicate (testpath/"kitex_gen/api/test.go").size, :zero?
  end
end
