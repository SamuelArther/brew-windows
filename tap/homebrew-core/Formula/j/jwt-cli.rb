class JwtCli < Formula
  desc "Super fast CLI tool to decode and encode JWTs built in Rust"
  homepage "https://github.com/mike-engel/jwt-cli"
  url "https://github.com/mike-engel/jwt-cli/releases/download/6.2.0/jwt-windows.tar.gz"
  sha256 "f8dc93223cf7f778e1d8c2787832b4a5f70511ad0073424d27bbdb3f071ddd4f"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "jwt.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/jwt --version")
  end
end
