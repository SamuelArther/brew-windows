class Yeet < Formula
  desc "Packaging tool that lets you declare build instructions in JavaScript"
  homepage "https://github.com/TecharoHQ/yeet"
  url "https://github.com/TecharoHQ/yeet/archive/refs/tags/v0.13.0.tar.gz"
  sha256 "971be1fd808473672e752e42f305176a5732183f2386e4bc689d39937fca0daf"
  license "MIT"
  head "https://github.com/TecharoHQ/yeet.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "0ac3e9573b455b58fb2063502e0b3168564ebdad6f722685a5f8e31425e06937"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[-X github.com/TecharoHQ/yeet.Version=#{version}]
    system "go", "build", *std_go_args(ldflags:), "./cmd/yeet"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/yeet -version")

    # Windows words ENOENT "The system cannot find the file specified".
    output = "open yeetfile.js: #{OS.windows? ? "The system cannot find the file specified" : "no such file or directory"}"
    assert_match output, shell_output("#{bin}/yeet 2>&1", 1)
  end
end
