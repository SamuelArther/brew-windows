class Swag < Formula
  desc "Automatically generate RESTful API documentation with Swagger 2.0 for Go"
  homepage "https://github.com/swaggo/swag"
  url "https://github.com/swaggo/swag/archive/refs/tags/v1.16.6.tar.gz"
  sha256 "d0193f08b829e1088753ff6d66d1205e22a6e7fd07ac28df5ecb001d9eb2c43d"
  license "MIT"
  head "https://github.com/swaggo/swag.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "d4184d75c23d1b19df7d9ddd1203fd97f197df71064ffeb44f5c1c92eaf0681d"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    # version patch PR, https://github.com/swaggo/swag/pull/2049
    inreplace "version.go", "1.16.4", version.to_s

    system "go", "build", *std_go_args, "./cmd/swag"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/swag --version")

    (testpath/"main.go").write <<~GO
      // Package main Simple API.
      // @title Simple API
      // @version 1.0.0
      // @description This is a simple API server.

      // @host localhost:8080
      // @BasePath /api/v1

      package main

      import "github.com/gin-gonic/gin"

      func main() {
        r := gin.Default()
        r.GET("/ping", func(c *gin.Context) {
          c.JSON(200, gin.H{"message": "pong"})
        })
        r.Run()
      }
    GO

    system bin/"swag", "init"
    assert_path_exists testpath/"docs/docs.go"
    assert_path_exists testpath/"docs/swagger.json"
  end
end
