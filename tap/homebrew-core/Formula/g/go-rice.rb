class GoRice < Formula
  desc "Easily embed resources like HTML, JS, CSS, images, and templates in Go"
  homepage "https://github.com/GeertJohan/go.rice"
  url "https://github.com/GeertJohan/go.rice/archive/refs/tags/v1.0.3.tar.gz"
  sha256 "dda8be9c9c594e164e664479001e7113d0f6571b3fc93253ef132096540f0673"
  license "BSD-2-Clause"
  head "https://github.com/GeertJohan/go.rice.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "93cd4373234f848c8c75c637b6e42530aa07264583d559909b78dfa571c0ca07"
  end

  depends_on "go" => [:build, :test]

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X main.BuildVersion=#{version}"
    system "go", "build", *std_go_args(ldflags:, output: bin/"rice"), "./rice"
  end

  test do
    (testpath/"testproject").mkpath
    cd "testproject" do
      (testpath/"testproject/main.go").write <<~GO
        package main

        import (
          "fmt"
          rice "github.com/GeertJohan/go.rice"
        )

        func main() {
          box := rice.MustFindBox("templates")
          str, err := box.String("test.txt")
          if err != nil {
            panic(err)
          }
          fmt.Print(str)
        }
      GO

      (testpath/"testproject/templates").mkpath
      (testpath/"testproject/templates/test.txt").write "Hello, rice!"

      # Initialize go module and get go.rice dependency
      system "go", "mod", "init", "testproject"
      system "go", "mod", "tidy"

      # Use go-rice to embed the resources
      system bin/"rice", "embed-go"

      system "go", "build", "-o", "testbin"
      assert_match "Hello, rice!", shell_output("./testbin")
    end
  end
end
