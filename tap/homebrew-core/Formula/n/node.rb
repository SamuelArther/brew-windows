class Node < Formula
  desc "JavaScript runtime built on V8"
  homepage "https://nodejs.org/"
  url "https://nodejs.org/dist/v26.12.0/node-v26.12.0-win-x64.zip"
  version "26.12.0"
  sha256 "50efaf01a08c520b4bbe499c09a8c5a5c370002c419c0d9da2c6815d3066a453"
  license "MIT"

  livecheck do
    url "https://nodejs.org/dist/"
    regex(%r{href=["']?v?(\d+(?:\.\d+)+)/?["' >]}i)
  end

  def install
    install_tree
    windows_exe "libexec/node.exe", "node"
    windows_cmd "libexec/npm.cmd", "npm"
    windows_cmd "libexec/npx.cmd", "npx"
  end

  test do
    path = testpath/"test.js"
    path.write "console.log('hello');"

    output = shell_output("#{bin}/node #{path}").strip
    assert_equal "hello", output
    output = shell_output("#{bin}/node -e \"console.log(new Intl.NumberFormat('en-EN').format(1234.56))\"").strip
    assert_equal "1,234.56", output

    output = shell_output("#{bin}/node -e \"console.log(new Intl.NumberFormat('de-DE').format(1234.56))\"").strip
    assert_equal "1.234,56", output

    output = shell_output("#{bin}/node -e \"console.log(new Temporal.Instant(0n).toString())\"").strip
    assert_equal "1970-01-01T00:00:00Z", output

    # make sure npm can find node
    ENV.delete "NVM_NODEJS_ORG_MIRROR"
    assert_path_exists bin/"npm.cmd", "npm must exist"
    npm_args = ["-ddd", "--cache=#{testpath}/npm_cache", "--build-from-source"]
    system bin/"npm", *npm_args, "install", "npm@latest"
    system bin/"npm", *npm_args, "install", "nan"
    assert_path_exists bin/"npx.cmd", "npx must exist"
    assert_match "< hello >", shell_output("#{bin}/npx --yes cowsay hello")

    # Test `uvwasi` is linked correctly
    (testpath/"wasi-smoke-test.mjs").write <<~JAVASCRIPT
      import { WASI } from 'node:wasi';

      // Minimal WASM that:
      //   - imports wasi proc_exit(i32)->()
      //   - exports memory (required by Node's WASI binding)
      //   - exports _start which calls proc_exit(42)
      const wasmBytes = new Uint8Array([
        // \\0asm + version
        0x00,0x61,0x73,0x6d, 0x01,0x00,0x00,0x00,

        // Type section: 2 types: (i32)->() and ()->()
        0x01,0x08, 0x02,
          0x60,0x01,0x7f,0x00,
          0x60,0x00,0x00,

        // Import section: wasi_snapshot_preview1.proc_exit : func(type 0)
        0x02,0x24, 0x01,
          0x16, // module name len = 22
            0x77,0x61,0x73,0x69,0x5f,0x73,0x6e,0x61,0x70,0x73,0x68,0x6f,0x74,
            0x5f,0x70,0x72,0x65,0x76,0x69,0x65,0x77,0x31,
          0x09, // name len = 9
            0x70,0x72,0x6f,0x63,0x5f,0x65,0x78,0x69,0x74,
          0x00, // import kind = func
          0x00, // type index 0

        // Function section: 1 function (type index 1 = ()->())
        0x03,0x02, 0x01, 0x01,

        // Memory section: one memory with min=1 page; export later
        0x05,0x03, 0x01, 0x00, 0x01,

        // Export section: export "_start" (func 1) and "memory" (mem 0)
        0x07,0x13, 0x02,
          0x06, 0x5f,0x73,0x74,0x61,0x72,0x74, 0x00, 0x01,
          0x06, 0x6d,0x65,0x6d,0x6f,0x72,0x79, 0x02, 0x00,

        // Code section: body for func 1: i32.const 42; call 0; end
        0x0a,0x08, 0x01,
          0x06, 0x00, 0x41,0x2a, 0x10,0x00, 0x0b
      ]);

      const wasi = new WASI({
        version: 'preview1',
        returnOnExit: true
      });

      const { instance } = await WebAssembly.instantiate(wasmBytes, wasi.getImportObject());

      // This should return 42 if uvwasi is correctly linked & wired.
      const rc = wasi.start(instance);
      if (rc === 42) {
        console.log('PASS: uvwasi proc_exit(42) worked (exitCode=42)');
        process.exit(0);
      } else {
        console.error('FAIL: unexpected return', rc);
        process.exit(2);
      }
    JAVASCRIPT

    system bin/"node", "wasi-smoke-test.mjs"
  end
end
