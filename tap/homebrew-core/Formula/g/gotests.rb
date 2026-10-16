class Gotests < Formula
  desc "Automatically generate Go test boilerplate from your source code"
  homepage "https://github.com/cweill/gotests"
  url "https://github.com/cweill/gotests/archive/refs/tags/v1.9.0.tar.gz"
  sha256 "1a36874dd5beec211e9b9aaf7d72be8839e76b5ad0a002cb4e83b80ad948697b"
  license "Apache-2.0"
  head "https://github.com/cweill/gotests.git", branch: "develop"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "3ddc21a031c02a3685eeae1567ab2fd1e7b1f89b06deae59df2a900a0119218d"
  end

  depends_on "go" => [:build, :test]

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./gotests"
  end

  test do
    (testpath/"test.go").write <<~GO
      package main

      func add(x int, y int) int {
      	return x + y
      }
    GO
    expected = <<~GO
      Generated Test_add
      package main

      import "testing"

      func Test_add(t *testing.T) {
      	type args struct {
      		x int
      		y int
      	}
      	tests := []struct {
      		name string
      		args args
      		want int
      	}{
      		// TODO: Add test cases.
      	}
      	for _, tt := range tests {
      		t.Run(tt.name, func(t *testing.T) {
      			if got := add(tt.args.x, tt.args.y); got != tt.want {
      				t.Errorf("add() = %v, want %v", got, tt.want)
      			}
      		})
      	}
      }
    GO
    assert_equal expected, shell_output("#{bin}/gotests -all test.go")
  end
end
