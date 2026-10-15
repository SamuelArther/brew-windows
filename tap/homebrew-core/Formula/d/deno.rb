class Deno < Formula
  desc "Secure runtime for JavaScript and TypeScript"
  homepage "https://deno.com/"
  version "2.9.7"
  url "https://github.com/denoland/deno/releases/download/v2.9.7/deno-x86_64-pc-windows-msvc.zip"
  sha256 "a0c3101b4158d1dfb7d6a78a7bf0f3de80c96bb423c152beec8beb22786f2238"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "deno.exe"
  end

  test do
    (testpath/"hello.ts").write <<~TYPESCRIPT
      console.log("hello", "deno");
    TYPESCRIPT
    assert_match "hello deno", shell_output("#{bin}/deno run hello.ts")
    assert_match version.to_s, shell_output("#{bin}/deno --version")
  end
end
