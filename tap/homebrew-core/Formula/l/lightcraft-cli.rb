class LightcraftCli < Formula
  desc "Headless LightCraft CLI and MCP server to develop and export photos"
  homepage "https://getartcraft.com/apps/lightcraft"
  url "https://github.com/storytold/lightcraft/archive/refs/tags/v0.19.0.tar.gz"
  sha256 "3d09c282ee23d4369a18044e810d15b55d5067da86bd6aba348289765f0f631e"
  license any_of: ["MIT", "Apache-2.0"]
  head "https://github.com/storytold/lightcraft.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "4b64f4024f158b6a022dd66554a588e1dd3bd8a34d5653cb27f72f4fa7b87073"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "apps/lightcraft-cli", features: "heif")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/lightcraft-cli --version")

    system bin/"lightcraft-cli", "synth-merge", "hdr", "-o", testpath
    merge = JSON.parse(shell_output("#{bin}/lightcraft-cli merge hdr bracket-0.dng bracket-1.dng bracket-2.dng"))
    assert_equal [1800, 1200], [merge["width"], merge["height"]]
    assert_equal [-2, 0, 2], merge["evExif"].map(&:round)

    system bin/"lightcraft-cli", "render", "bracket-0-HDR.dng", "-o", "test.png",
           "--set", "light.exposure=0.5", "--size", "120"
    png = (testpath/"test.png").binread(24)
    assert_equal "\x89PNG".b, png[0, 4]
    assert_equal [120, 80], png[16, 8].unpack("NN")
  end
end
