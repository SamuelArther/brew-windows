class Hotbuild < Formula
  desc "Cross platform hot compilation tool for go"
  homepage "https://hotbuild.rustpub.com/"
  url "https://github.com/wandercn/hotbuild/archive/refs/tags/v1.0.9.tar.gz"
  sha256 "7e8c5c52269344d12d4dc83ae4f472f8aec05faad76379c844dc21c2da44704c"
  license "MulanPSL-2.0"
  head "https://github.com/wandercn/hotbuild.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e344e076a3fba8c1c757d5e7cac25a52856f60709f4b778c6bdea8bbb9840b8a"
  end

  depends_on "go" => :build

  # Bump version
  patch do
    url "https://github.com/wandercn/hotbuild/commit/1b04ea4e9e1327ef4d462256072d72f4f37040cb.patch?full_index=1"
    sha256 "b0bbcdf106914307265b4ac81d73667a8b2d4c2fd688cc76dd1e303f690b4021"
    type :unofficial
    resolves "https://github.com/wandercn/hotbuild/pull/15"
  end

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    # On Windows the default build command writes tmp/tmp_bin.exe.
    output = "buildcmd = \"go build -o tmp/tmp_bin#{".exe" if OS.windows?}\""
    system bin/"hotbuild", "initconf"
    assert_match output, (testpath/".hotbuild.toml").read

    assert_match version.to_s, shell_output("#{bin}/hotbuild version")
  end
end
