// SPDX-License-Identifier: MIT
// Compatible with OpenZeppelin Contracts for Cairo ^2.0.0

#[starknet::contract]
mod Devil {
    use openzeppelin::access::ownable::OwnableComponent;
    use openzeppelin::introspection::src5::SRC5Component;
    use openzeppelin::security::pausable::PausableComponent;
    use openzeppelin::token::erc1155::ERC1155Component;
    use openzeppelin::upgrades::interface::IUpgradeable;
    use openzeppelin::upgrades::UpgradeableComponent;
    use starknet::{ClassHash, ContractAddress, get_caller_address};

    component!(path: ERC1155Component, storage: erc1155, event: ERC1155Event);
    component!(path: SRC5Component, storage: src5, event: SRC5Event);
    component!(path: PausableComponent, storage: pausable, event: PausableEvent);
    component!(path: OwnableComponent, storage: ownable, event: OwnableEvent);
    component!(path: UpgradeableComponent, storage: upgradeable, event: UpgradeableEvent);

    // External
    #[abi(embed_v0)]
    impl ERC1155Impl = ERC1155Component::ERC1155Impl<ContractState>;
    #[abi(embed_v0)]
    impl ERC1155CamelImpl = ERC1155Component::ERC1155CamelImpl<ContractState>;
    #[abi(embed_v0)]
    impl PausableImpl = PausableComponent::PausableImpl<ContractState>;
    #[abi(embed_v0)]
    impl OwnableMixinImpl = OwnableComponent::OwnableMixinImpl<ContractState>;

    // Internal
    impl ERC1155InternalImpl = ERC1155Component::InternalImpl<ContractState>;
    impl PausableInternalImpl = PausableComponent::InternalImpl<ContractState>;
    impl OwnableInternalImpl = OwnableComponent::InternalImpl<ContractState>;
    impl UpgradeableInternalImpl = UpgradeableComponent::InternalImpl<ContractState>;

    #[storage]
    struct Storage {
        #[substorage(v0)]
        erc1155: ERC1155Component::Storage,
        #[substorage(v0)]
        src5: SRC5Component::Storage,
        #[substorage(v0)]
        pausable: PausableComponent::Storage,
        #[substorage(v0)]
        ownable: OwnableComponent::Storage,
        #[substorage(v0)]
        upgradeable: UpgradeableComponent::Storage,
    }

    #[event]
    #[derive(Drop, starknet::Event)]
    enum Event {
        #[flat]
        ERC1155Event: ERC1155Component::Event,
        #[flat]
        SRC5Event: SRC5Component::Event,
        #[flat]
        PausableEvent: PausableComponent::Event,
        #[flat]
        OwnableEvent: OwnableComponent::Event,
        #[flat]
        UpgradeableEvent: UpgradeableComponent::Event,
    }

    #[constructor]
    fn constructor(ref self: ContractState, owner: ContractAddress) {
        self.erc1155.initializer("ipfs://bafybeicctuar4anr44grpdsnuhlkknilvaso72mqsvc27zucaxijilnbtm/");
        self.ownable.initializer(owner);
    }

    impl ERC1155HooksImpl of ERC1155Component::ERC1155HooksTrait<ContractState> {
        fn before_update(
            ref self: ERC1155Component::ComponentState<ContractState>,
            from: ContractAddress,
            to: ContractAddress,
            token_ids: Span<u256>,
            values: Span<u256>,
        ) {
            let contract_state = self.get_contract();
            contract_state.pausable.assert_not_paused();
        }
    }
    
    #[generate_trait]
    #[abi(per_item)]
    impl ExternalImpl of ExternalTrait {
        #[external(v0)]
        fn pause(ref self: ContractState) {
            self.ownable.assert_only_owner();
            self.pausable.pause();
        }

        #[external(v0)]
        fn unpause(ref self: ContractState) {
            self.ownable.assert_only_owner();
            self.pausable.unpause();
        }

        #[external(v0)]
        fn burn(ref self: ContractState, account: ContractAddress, token_id: u256, value: u256) {
            let caller = get_caller_address();
            if account != caller {
                assert(self.erc1155.is_approved_for_all(account, caller), ERC1155Component::Errors::UNAUTHORIZED);
            }
            self.erc1155.burn(account, token_id, value);
        }

        #[external(v0)]
        fn batch_burn(
            ref self: ContractState,
            account: ContractAddress,
            token_ids: Span<u256>,
            values: Span<u256>,
        ) {
            let caller = get_caller_address();
            if account != caller {
                assert(self.erc1155.is_approved_for_all(account, caller), ERC1155Component::Errors::UNAUTHORIZED);
            }
            self.erc1155.batch_burn(account, token_ids, values);
        }

        #[external(v0)]
        fn batchBurn(
            ref self: ContractState,
            account: ContractAddress,
            tokenIds: Span<u256>,
            values: Span<u256>,
        ) {
            self.batch_burn(account, tokenIds, values);
        }

        #[external(v0)]
        fn mint(
            ref self: ContractState,
            account: ContractAddress,
            token_id: u256,
            value: u256,
            data: Span<felt252>,
        ) {
            self.ownable.assert_only_owner();
            self.erc1155.mint_with_acceptance_check(account, token_id, value, data);
        }

        #[external(v0)]
        fn selfMint(
            ref self: ContractState,
            account: ContractAddress,
            token_id: u256,
            value: u256,
            data: Span<felt252>,
        ) {
            self.erc1155.mint_with_acceptance_check(account, token_id, value, data);
        }

        #[external(v0)]
        fn batch_mint(
            ref self: ContractState,
            account: ContractAddress,
            token_ids: Span<u256>,
            values: Span<u256>,
            data: Span<felt252>,
        ) {
            self.ownable.assert_only_owner();
            self.erc1155.batch_mint_with_acceptance_check(account, token_ids, values, data);
        }

        #[external(v0)]
        fn batchMint(
            ref self: ContractState,
            account: ContractAddress,
            tokenIds: Span<u256>,
            values: Span<u256>,
            data: Span<felt252>,
        ) {
            self.batch_mint(account, tokenIds, values, data);
        }

        #[external(v0)]
        fn set_base_uri(ref self: ContractState, base_uri: ByteArray) {
            self.ownable.assert_only_owner();
            self.erc1155._set_base_uri(base_uri);
        }

        #[external(v0)]
        fn setBaseUri(ref self: ContractState, baseUri: ByteArray) {
            self.set_base_uri(baseUri);
        }

        #[external(v0)]
        fn uri(self: @ContractState, token_id: u256) -> ByteArray {
            // Convert token_id to 64-character lowercase hex with zero padding
            format!("ipfs://bafybeicctuar4anr44grpdsnuhlkknilvaso72mqsvc27zucaxijilnbtm/{}.json", token_id)
        }

    }
    




    //
    // Upgradeable
    //
    
    #[abi(embed_v0)]
    impl UpgradeableImpl of IUpgradeable<ContractState> {
        fn upgrade(ref self: ContractState, new_class_hash: ClassHash) {
            self.ownable.assert_only_owner();
            self.upgradeable.upgrade(new_class_hash);
        }
    }

}
