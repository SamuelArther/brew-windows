class Screenpipe < Formula
  desc "Library to build personalized AI powered by what you've seen, said, or heard"
  homepage "https://github.com/mediar-ai/screenpipe"
  url "https://github.com/mediar-ai/screenpipe/archive/refs/tags/v0.2.13.tar.gz"
  sha256 "eb3599daabc1312b5c1a7799c1ec8ab715aa02d9216a6aa42d930039c84a70c9"
  license "MIT"

  # Unable to update as newer versions need Bun.
  # Older version used by formula doesn't build with newer Rust.
  deprecate! date: "2025-08-25", because: :does_not_build
  disable! date: "2026-08-25", because: :does_not_build

  depends_on "cmake" => :build
  depends_on "pkgconf" => :build
  depends_on "rust" => :build
  depends_on "ffmpeg"

  uses_from_macos "llvm" # for libclang

  on_macos do
    depends_on macos: :sonoma
  end

  on_linux do
    depends_on "alsa-lib"
    depends_on "dbus"
    depends_on "libxcb"
    depends_on "openssl@3"
    depends_on "tesseract"
    depends_on "xz"
  end

  def install
    features = ["metal", "pipes"] if OS.mac? && Hardware::CPU.arm?
    system "cargo", "install", *std_cargo_args(path: "screenpipe-server", features:)
    lib.install "screenpipe-vision/lib/libscreenpipe_#{Hardware::CPU.arch}.dylib" if OS.mac?
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/screenpipe -V")

    log_file = testpath/".screenpipe/screenpipe.#{time.strftime("%Y-%m-%d")}.log"
    pid = spawn bin/"screenpipe", "--disable-vision", "--disable-audio", "--disable-telemetry"
    sleep 200

    assert_path_exists log_file
    assert_match(/INFO.*screenpipe/, File.read(log_file))
  ensure
    Process.kill("TERM", pid)
    Process.wait(pid)
  end
end
