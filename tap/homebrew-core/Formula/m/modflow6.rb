class Modflow6 < Formula
  desc "USGS modular hydrologic model"
  homepage "https://www.usgs.gov/software/modflow-6-usgs-modular-hydrologic-model"
  url "https://github.com/MODFLOW-ORG/modflow6/archive/refs/tags/6.8.1.tar.gz"
  sha256 "16b9368d582c66de83106a4c075d22c2a7a08daa7d8dfb7f40e21a5d983be699"
  license "CC0-1.0"
  head "https://github.com/MODFLOW-ORG/modflow6.git", branch: "develop"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e15865d3d86184f580443b2cd076359c75ff6d0aef2dc22e16d1e8fe20bb909e"
  end

  depends_on "meson" => :build
  depends_on "ninja" => :build
  depends_on "gcc" # for gfortran

  deny_network_access!

  def install
    system "meson", "setup", "build", *std_meson_args
    system "meson", "compile", "-C", "build", "--verbose"
    system "meson", "install", "-C", "build"
    pkgshare.install ".mf6minsim" => "mf6minsim"

    # zbud6 is a utility built by the default meson targets and is not packaged
    rm bin/"zbud6"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mf6 --version")

    cp_r pkgshare/"mf6minsim/.", testpath
    system bin/"mf6"
    assert_match "Normal termination of simulation", (testpath/"mfsim.lst").read

    # run the same one-step simulation through libmf6
    rm testpath/"mfsim.lst"
    (testpath/"test.c").write <<~C
      int initialize(void), update(void), finalize(void);
      int main(void) { return initialize() || update() || finalize(); }
    C
    system ENV.cc, "test.c", "-L#{lib}", "-lmf6", "-Wl,-rpath,#{lib}", "-o", "test"
    system "./test"
    assert_match "Normal termination of simulation", (testpath/"mfsim.lst").read
  end
end
