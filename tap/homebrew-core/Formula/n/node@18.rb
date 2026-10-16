class NodeAT18 < Formula
  desc "Open-source, cross-platform JavaScript runtime environment"
  homepage "https://nodejs.org/"
  url "https://nodejs.org/dist/v18.20.8/node-v18.20.8.tar.xz"
  sha256 "36a7bf1a76d62ce4badd881ee5974a323c70e1d8d19165732684e145632460d9"
  license "MIT"

  keg_only :versioned_formula

  # https://github.com/nodejs/release#release-schedule
  deprecate! date: "2024-10-29", because: :unsupported
  disable! date: "2025-10-29", because: :unsupported

  depends_on "pkgconf" => :build
  depends_on "python-setuptools" => :build
  depends_on "python@3.13" => :build
  depends_on "brotli"
  depends_on "c-ares"
  depends_on "icu4c@77"
  depends_on "libnghttp2"
  depends_on "libuv"
  depends_on "openssl@3"

  uses_from_macos "python"
  uses_from_macos "zlib"

  # Backport support for ICU 76+
  patch do
    url "https://github.com/nodejs/node/commit/81517faceac86497b3c8717837f491aa29a5e0f9.patch?full_index=1"
    sha256 "79a5489617665c5c88651a7dc364b8967bebdea5bdf361b85572d041a4768662"
    type :backport
    resolves "https://github.com/nodejs/node/pull/55563"
  end

  def install
    # Fix to avoid fdopen() redefinition for vendored `zlib`
    # Too many commits to backport, so apply a workaround
    if OS.mac? && DevelopmentTools.clang_build_version >= 1700
      inreplace "deps/v8/third_party/zlib/zutil.h",
                "#        define fdopen(fd,mode) NULL /* No fdopen() */",
                ""
    end

    # make sure subprocesses spawned by make are using our Python 3
    ENV["PYTHON"] = python3

    args = %W[
      --prefix=#{prefix}
      --with-intl=system-icu
      --shared-libuv
      --shared-nghttp2
      --shared-openssl
      --shared-zlib
      --shared-brotli
      --shared-cares
      --shared-libuv-includes=#{formula_opt_include("libuv")}
      --shared-libuv-libpath=#{formula_opt_lib("libuv")}
      --shared-nghttp2-includes=#{formula_opt_include("libnghttp2")}
      --shared-nghttp2-libpath=#{formula_opt_lib("libnghttp2")}
      --shared-openssl-includes=#{formula_opt_include("openssl@3")}
      --shared-openssl-libpath=#{formula_opt_lib("openssl@3")}
      --shared-brotli-includes=#{formula_opt_include("brotli")}
      --shared-brotli-libpath=#{formula_opt_lib("brotli")}
      --shared-cares-includes=#{formula_opt_include("c-ares")}
      --shared-cares-libpath=#{formula_opt_lib("c-ares")}
      --openssl-use-def-ca-store
    ]

    system "./configure", *args
    system "make", "install"
  end

  post_install_steps do
    write_file "lib/node_modules/npm/npmrc", "prefix = {{HOMEBREW_PREFIX}}\n", base: :prefix
  end

  test do
    # Make sure Mojave does not have `CC=llvm_clang`.
    ENV.clang if OS.mac?

    path = testpath/"test.js"
    path.write "console.log('hello');"

    output = shell_output("#{bin}/node #{path}").strip
    assert_equal "hello", output
    output = shell_output("#{bin}/node -e 'console.log(new Intl.NumberFormat(\"en-EN\").format(1234.56))'").strip
    assert_equal "1,234.56", output

    output = shell_output("#{bin}/node -e 'console.log(new Intl.NumberFormat(\"de-DE\").format(1234.56))'").strip
    assert_equal "1.234,56", output

    # make sure npm can find node
    ENV.prepend_path "PATH", opt_bin
    ENV.delete "NVM_NODEJS_ORG_MIRROR"
    assert_equal which("node"), opt_bin/"node"
    assert_path_exists bin/"npm", "npm must exist"
    assert_predicate bin/"npm", :executable?, "npm must be executable"
    npm_args = ["-ddd", "--cache=#{HOMEBREW_CACHE}/npm_cache", "--build-from-source"]
    system bin/"npm", *npm_args, "install", "npm@latest"
    system bin/"npm", *npm_args, "install", "ref-napi"
    assert_path_exists bin/"npx", "npx must exist"
    assert_predicate bin/"npx", :executable?, "npx must be executable"
    assert_match "< hello >", shell_output("#{bin}/npx --yes cowsay hello")
  end
end
