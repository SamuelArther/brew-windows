class Save3dsFuse < Formula
  desc "Extract/Import/FUSE for 3DS save/extdata/database"
  homepage "https://github.com/wwylele/save3ds"
  url "https://github.com/wwylele/save3ds/archive/refs/tags/v1.4.0.tar.gz"
  sha256 "3bf47e34db1f3e5162df5b8e67a5673b473b37bf0eaa729be28e5e7a62212858"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "cb1274bdbb4209371f76c9315354ab91e143b5de7d601f11610ad7f50190ee0d"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", "--no-default-features", *std_cargo_args(path: "save3ds_fuse")
  end

  test do
    # `save3ds_fuse` requires a mount path to operate correctly
    assert_match "Please specify one mount path", shell_output(bin/"save3ds_fuse")

    (testpath/"testfile").write "test"
    output = shell_output("#{bin}/save3ds_fuse --bare testfile #{testpath}/tmp 2>&1", 1)
    assert_match "Out-of-bound access, caused by corrupted data", output
  end
end
