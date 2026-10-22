class Librespot < Formula
  desc "Open Source Spotify client library"
  homepage "https://github.com/librespot-org/librespot"
  url "https://github.com/librespot-org/librespot/archive/refs/tags/v0.8.0.tar.gz"
  sha256 "0e4922997e1c67d27b3f50dcc388ecb8a3c08eba23b764879071f6e9e8c07ec7"
  license "MIT"
  head "https://github.com/librespot-org/librespot.git", branch: "dev"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "4dedfa287682611a48962615ae1a1db6adba54029c2a07edac31fbdab40ef06a"
  end

  depends_on "pkgconf" => :build
  depends_on "rust" => :build

  on_linux do
    depends_on "alsa-lib"
    depends_on "openssl@4"
  end

  # Apply open PR to update openssl and openssl-sys for OpenSSL 4
  patch do
    url "https://github.com/librespot-org/librespot/commit/5ceaa54fcfca8ebe522e3463fb110dd49dade846.patch?full_index=1"
    sha256 "3cd869925d498406c83878e55e6f26fc59b51554f8550b10f85bacf9168be9d8"
    type :unofficial
    resolves "https://github.com/librespot-org/librespot/pull/1707"
  end

  allow_network_access! :test

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    if OS.mac?
      ENV["COREAUDIO_SDK_PATH"] = MacOS.sdk_path.to_s
      args = %w[--no-default-features]
      # We use `with-dns-sd` on macOS since system Bonjour can be used.
      # Linux requires Avahi which isn't well maintained so better to use libmdns.
      features = %w[native-tls rodio-backend with-dns-sd]
    end

    system "cargo", "install", *args, *std_cargo_args(features:)
  end

  test do
    require "open3"
    require "timeout"

    Open3.popen3({ "RUST_LOG" => "DEBUG" }, bin/"librespot", "-v") do |_, _, stderr, wait_thr|
      Timeout.timeout(5) do
        stderr.each do |line|
          refute_match "ERROR", line
          break if line.include?("Zeroconf server listening")
        end
      end
    ensure
      Process.kill("INT", wait_thr.pid)
    end
  end
end
