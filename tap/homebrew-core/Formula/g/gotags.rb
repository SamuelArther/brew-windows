class Gotags < Formula
  desc "Tag generator for Go, compatible with ctags"
  homepage "https://github.com/jstemmer/gotags"
  url "https://github.com/jstemmer/gotags/archive/refs/tags/v1.4.1.tar.gz"
  sha256 "2df379527eaa7af568734bc4174febe7752eb5af1b6194da84cd098b7c873343"
  license "MIT"
  head "https://github.com/jstemmer/gotags.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "2c40fbd35ded9bf78eb56390f69a74bb80b3f7e171d1816f4fbe5f46b5dcf805"
  end

  depends_on "go" => :build

  deny_network_access!

  def install
    ENV["GO111MODULE"] = "auto"
    system "go", "build", *std_go_args
  end

  test do
    (testpath/"test.go").write <<~GO
      package main

      type Foo struct {
          Bar int
      }
    GO

    assert_match(/^Bar.*test.go.*$/, shell_output("#{bin}/gotags #{testpath}/test.go"))
  end
end
