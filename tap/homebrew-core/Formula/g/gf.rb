class Gf < Formula
  desc "App development framework of Golang"
  homepage "https://goframe.org"
  url "https://github.com/gogf/gf/archive/refs/tags/v2.10.3.tar.gz"
  sha256 "548e000382e013d853b8b3e360aa48e5bd23689a3a2eb3946d6f0b17e8339c5e"
  license "MIT"
  head "https://github.com/gogf/gf.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "2ad87f1a31a4bc1ace293c1df0c0832b623b456c148e63a41b6f83eb182c1f2c"
  end

  depends_on "go" => [:build, :test]

  deny_network_access!

  def fetch
    system "go", "mod", "download", "-C", "cmd/gf"
  end

  def install
    cd "cmd/gf" do
      system "go", "build", *std_go_args
    end
  end

  test do
    output = shell_output("#{bin}/gf --version 2>&1")
    assert_match "v#{version}\nWelcome to GoFrame!", output
    assert_match "GF Version(go.mod): cannot find go.mod", output

    output = shell_output("#{bin}/gf init test 2>&1")
    assert_match "you can now run \"cd test && gf run main.go\" to start your journey, enjoy!", output
  end
end
