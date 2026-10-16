class BotanAT2 < Formula
  desc "Cryptographic algorithms and formats library in C++"
  homepage "https://botan.randombit.net/"
  url "https://botan.randombit.net/releases/Botan-2.19.5.tar.xz"
  sha256 "dfeea0e0a6f26d6724c4af01da9a7b88487adb2d81ba7c72fcaf52db522c9ad4"
  license "BSD-2-Clause"

  keg_only :versioned_formula

  # Botan2 is currently scheduled to reach end of life at the end of 2024
  # Ref: https://botan.randombit.net/#releases
  deprecate! date: "2024-12-31", because: :unsupported
  disable! date: "2025-12-31", because: :unsupported

  depends_on "pkgconf" => :build
  depends_on "python@3.14"
  depends_on "sqlite"

  uses_from_macos "bzip2"
  uses_from_macos "zlib"

  def install
    ENV.cxx11

    args = %W[
      --prefix=#{prefix}
      --docdir=share/doc
      --with-zlib
      --with-bzip2
      --with-sqlite3
    ]

    system python3, "configure.py", *args
    system "make", "install"
  end

  test do
    (testpath/"test.txt").write "Homebrew"
    (testpath/"testout.txt").write shell_output("#{bin}/botan base64_enc test.txt")
    assert_match "Homebrew", shell_output("#{bin}/botan base64_dec testout.txt")
  end
end
