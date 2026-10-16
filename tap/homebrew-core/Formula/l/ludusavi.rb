class Ludusavi < Formula
  desc "Backup tool for PC game saves"
  homepage "https://github.com/mtkennerly/ludusavi"
  url "https://github.com/mtkennerly/ludusavi/archive/refs/tags/v0.31.0.tar.gz"
  sha256 "e7dcfa4fb17fcd2be9ef34d55fe0ca0d3f79fdf570107aeaa5b4e76910e2f8a1"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a3d34cae6f7a9ef7e54163dd796b882d38f4ab797a5ea8b7ef05bb92b52150f3"
  end

  depends_on "pkgconf" => :build
  depends_on "rust" => :build

  uses_from_macos "bzip2"

  on_linux do
    depends_on "glib"
    depends_on "gtk+3"
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ludusavi -V")
    assert_empty shell_output("#{bin}/ludusavi backups").strip
  end
end
