class Hz < Formula
  desc "Golang HTTP framework for microservices"
  homepage "https://www.cloudwego.io"
  url "https://github.com/cloudwego/hertz/archive/refs/tags/cmd/hz/v0.9.7.tar.gz"
  sha256 "c2d894db10414648cd4d131ca1405cb1fde0dcdd3287fbd4c4623c9916ae6717"
  license "Apache-2.0"
  head "https://github.com/cloudwego/hertz.git", branch: "develop"

  livecheck do
    url :stable
    regex(%r{^cmd/hz/v?(\d+(?:\.\d+)+)$}i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c7d663ca7f3bcd2b7d43d04f1818aea2902739706f60626e268c3e06b52b75db"
  end

  depends_on "go" => [:build, :test]

  deny_network_access!

  def fetch
    system "go", "mod", "download", "-C", "cmd/hz"
  end

  def install
    cd "cmd/hz" do
      system "go", "build", *std_go_args
    end
    bin.install_symlink bin/"hz" => "thrift-gen-hertz"
    bin.install_symlink bin/"hz" => "protoc-gen-hertz"
  end

  test do
    ENV["GOPATH"] = testpath

    output = shell_output("#{bin}/hz --version 2>&1")
    assert_match "hz version v#{version}", output

    system bin/"hz", "new", "--mod=test"
    assert_path_exists testpath/"main.go"
    refute_predicate (testpath/"main.go").size, :zero?
  end
end
