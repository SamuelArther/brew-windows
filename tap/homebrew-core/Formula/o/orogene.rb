class Orogene < Formula
  desc "`node_modules/` package manager and utility toolkit"
  homepage "https://orogene.dev"
  url "https://github.com/orogene/orogene/archive/refs/tags/v0.3.34.tar.gz"
  sha256 "d4e50c2c3965e62160cf6a15db3734e4a847ca79629599fdd5ce30579aaae9a3"
  license "Apache-2.0"

  # https://github.com/orogene/orogene/commit/7793618494db676bcfd3364b85fb21452a6c0bdf
  disable! date: "2026-08-13", because: :unsupported

  depends_on "pkgconf" => :build
  depends_on "rust" => :build

  on_linux do
    depends_on "openssl@3"
  end

  # rust 1.80 build patch
  patch do
    url "https://github.com/orogene/orogene/commit/2f774bb5b1067fb0f5f827140aff328190af0452.patch?full_index=1"
    sha256 "c91711588a6fddee3055a356723a8a044cd82d287c59a7cf83802129d2ffa89b"
    type :unofficial
    resolves "https://github.com/orogene/orogene/pull/315"
  end

  def install
    # Ensure that the `openssl` crate picks up the intended library.
    ENV["OPENSSL_DIR"] = formula_opt_prefix("openssl@3")

    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/oro --version")

    system bin/"oro", "ping"
  end
end
